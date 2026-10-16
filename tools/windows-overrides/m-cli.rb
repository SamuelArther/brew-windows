class MCli < Formula
  desc "Swiss Army Knife of system commands (network, disk, display, battery)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "m", "m"
  end

  test do
    assert_match "Swiss Army Knife", shell_output("#{bin}/m --help")
    assert_match "m-cli version", shell_output("#{bin}/m --version")
    assert_match "Processor", shell_output("#{bin}/m info")
    assert_match(/Fixed|Removable/, shell_output("#{bin}/m disk list"))
    refute_match(/No such file|command not found/, shell_output("#{bin}/m info"))
  end
end
