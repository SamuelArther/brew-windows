class LinuxPam < Formula
  desc "Authentication helpers (PAM) on Windows accounts"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "unix_chkpwd", "unix_chkpwd"
    windows_exe "libexec/wintools.exe", "faillock", "faillock"
    windows_exe "libexec/wintools.exe", "mkhomedir_helper", "mkhomedir_helper"
  end

  def caveats
    <<~EOS
      These are Linux-PAM's helper programs on Windows' own authentication: unix_chkpwd checks
      passwords with LogonUser, faillock reads failed logons from the Security log (elevated
      prompt) and unlocks accounts, mkhomedir_helper creates user profiles. Windows has no PAM
      module stack; programs authenticate through SSPI or LogonUser.
    EOS
  end

  test do
    assert_match "Usage: mkhomedir_helper <username>", shell_output("#{bin}/mkhomedir_helper 2>&1", 14)
    shell_output("#{bin}/mkhomedir_helper no_such_user_brew 2>&1", 10)
    pipe_output("#{bin}/unix_chkpwd no_such_user_brew nullok", "secret", 10)
    assert_match "When", shell_output("#{bin}/faillock")
  end
end
