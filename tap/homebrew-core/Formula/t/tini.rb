class Tini < Formula
  desc "Tiny but valid init for containers"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "tini", "tini"
    windows_exe "libexec/wintools.exe", "tini-static", "tini-static"
    windows_exe "libexec/wintools.exe", "docker-init", "docker-init"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tini --version")
    assert_match "hello", shell_output("#{bin}/tini -- cmd /c echo hello")
    shell_output("#{bin}/tini -- cmd /c exit 1", 1)
    shell_output("#{bin}/tini -e 3 -- cmd /c exit 3")
    assert_match "hello", shell_output("#{bin}/tini-static -- cmd /c echo hello")
    assert_match "hello", shell_output("#{bin}/docker-init -- cmd /c echo hello")
  end
end
