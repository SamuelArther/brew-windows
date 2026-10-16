class Nef < Formula
  desc "Steroids for Xcode Playgrounds"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "nef", "nef"
  end

  test do
    assert_match "markdown", shell_output("#{bin}/nef --help")
    (testpath/"Docs.playground").mkpath
    (testpath/"Docs.playground/Contents.swift").write <<~SWIFT
      // nef:begin:hidden
      import Secret
      // nef:end
      //: # Hello
      let answer = 42
    SWIFT
    system bin/"nef", "markdown", "--project", testpath, "--output", testpath/"out"
    md = (testpath/"out/Docs/Docs.md").read
    assert_match "# Hello", md
    assert_match "```swift\nlet answer = 42\n```", md
    refute_match "Secret", md
  end
end
