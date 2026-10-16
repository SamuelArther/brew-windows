class Apt < Formula
  desc "Advanced Package Tool commands for winget"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "apt", "apt"
    windows_exe "libexec/wintools.exe", "apt-get", "apt-get"
    windows_exe "libexec/wintools.exe", "apt-cache", "apt-cache"
  end

  def caveats
    <<~EOS
      apt, apt-get and apt-cache manage Windows packages through winget;
      package names are winget ids such as Git.Git.
    EOS
  end

  test do
    assert_match "apt #{version}", shell_output("#{bin}/apt --version")
    assert_match "install", shell_output("#{bin}/apt-get --help")
    assert_match "winget install --id Git.Git --exact", shell_output("#{bin}/apt-get -s install Git.Git")
    assert_match "nothing to remove", shell_output("#{bin}/apt autoremove")
    assert_match "Invalid operation", shell_output("#{bin}/apt frobnicate 2>&1", 2)
  end
end
