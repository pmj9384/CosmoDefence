using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

// 제출용 APK 원버튼 빌드 — Player Settings(패키지명/세로 고정)까지 코드가 보장해
// "설정 빠뜨린 빌드"가 나올 수 없게 한다. 산출물: Builds/TongTongDefence.apk
public static class BuildScript
{
    private const string OutputPath = "Builds/TongTongDefence.apk";

    [MenuItem("Tools/Build Android APK")]
    public static void BuildAndroid()
    {
        // 제출 요건 설정 (빌드마다 강제 — Inspector 상태에 의존하지 않음)
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.pmj.tongtongdefence");
        PlayerSettings.productName = "TongTongDefence";   // 런처 표시 이름 (유저 지정: 영문)
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;   // 세로 전용 게임
        // IL2CPP + ARM64/ARMv7 (제출 사양): Mono/ARMv7만으론 64비트 전용 최신 폰에서 설치 거부됨
        // (실사례: 타인 폰 zip 전달 설치 실패 — 2026-07-06). NDK 필요, 빌드 시간 증가 감수
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;

        Directory.CreateDirectory("Builds");
        var options = new BuildPlayerOptions
        {
            // 씬 목록은 Build Settings(SSOT)를 그대로 읽는다 — 복사본을 들고 있다가
            // 로비 씬 추가를 못 따라가 "로비 없는 APK"가 나올 뻔한 실사례 (검수 v5, 2026-07-23)
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = OutputPath,
            target = BuildTarget.Android,      // 플랫폼 스위치 포함 (첫 실행은 리임포트로 수 분)
            options = BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            Debug.Log($"[BuildScript] APK 빌드 성공 → {OutputPath} ({report.summary.totalSize / (1024 * 1024)}MB)");
        else
            Debug.LogError($"[BuildScript] 빌드 실패: {report.summary.result}");
    }
}
