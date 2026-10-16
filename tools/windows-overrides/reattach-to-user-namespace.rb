class ReattachToUserNamespace < Formula
  desc "Reattach process (e.g., tmux) to the user's interactive session"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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
