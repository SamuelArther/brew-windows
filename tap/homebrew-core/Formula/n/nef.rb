class Nef < Formula
  desc "Steroids for Xcode Playgrounds"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
