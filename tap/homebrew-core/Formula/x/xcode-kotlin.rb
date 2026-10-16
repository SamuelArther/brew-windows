class XcodeKotlin < Formula
  desc "Kotlin/Native LLDB formatters and Visual Studio language support"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "xcode-kotlin", "xcode-kotlin"
  end

  test do
    output = shell_output("#{bin}/xcode-kotlin info --only")
    assert_match(/Bundled plugin version:\s*\d+\.\d+\.\d+/, output)
    assert_match(/Installed plugin version:\s*(?:(?:\d+)\.(?:\d+)\.(?:\d+)|none)/, output)
    assert_match(/Language spec installed:\s*(?:Yes|No)/, output)
    assert_match(/LLDB init installed:\s*(?:Yes|No)/, output)
    assert_match(/LLDB Xcode init sources main LLDB init:\s*(?:Yes|No)/, output)
    (testpath/"konan/tools").mkpath
    (testpath/"konan/tools/konan_lldb.py").write "# Kotlin/Native formatters\n"
    system bin/"xcode-kotlin", "install", "--konan-home", testpath/"konan"
    output = shell_output("#{bin}/xcode-kotlin info --only")
    assert_match(/LLDB init installed:\s*Yes/, output)
    assert_match(/Language spec installed:\s*Yes/, output)
    system bin/"xcode-kotlin", "uninstall"
    assert_match(/Installed plugin version:\s*none/, shell_output("#{bin}/xcode-kotlin info --only"))
  end
end
