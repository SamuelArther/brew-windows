class ReattachToUserNamespace < Formula
  desc "Reattach process (e.g., tmux) to the user's interactive session"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "reattach-to-user-namespace", "reattach-to-user-namespace"
  end

  test do
    assert_match "Hello World!", shell_output("#{bin}/reattach-to-user-namespace cmd /c echo Hello World!")
    assert_match "version 2.9", shell_output("#{bin}/reattach-to-user-namespace -v")
    assert_match "usage:", shell_output("#{bin}/reattach-to-user-namespace 2>&1", 1)
  end
end
