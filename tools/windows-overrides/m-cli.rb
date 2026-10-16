class MCli < Formula
  desc "Swiss Army Knife of system commands (network, disk, display, battery)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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
