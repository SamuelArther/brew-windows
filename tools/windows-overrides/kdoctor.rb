class Kdoctor < Formula
  desc "Environment diagnostics for Kotlin Multiplatform development"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "kdoctor", "kdoctor"
  end

  test do
    output = shell_output("#{bin}/kdoctor --team-ids")
    assert_match "Certificates are not found", output
    output = shell_output(bin/"kdoctor")
    assert_match "System", output
    assert_match "Java", output
    assert_match "Android Studio", output
    assert_match "Xcode", output
    assert_match "CocoaPods", output
    assert_match "Conclusion", output
    assert_match version.to_s, shell_output("#{bin}/kdoctor --version")
    assert_match "no such option", shell_output("#{bin}/kdoctor --bogus 2>&1", 2)
  end
end
