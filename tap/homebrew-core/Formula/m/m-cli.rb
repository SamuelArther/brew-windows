class MCli < Formula
  desc "Swiss Army Knife of system commands (network, disk, display, battery)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

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
