class Apt < Formula
  desc "Advanced Package Tool commands for winget"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

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
