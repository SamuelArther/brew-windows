class WaitOn < Formula
  desc "Lets shell scripts wait for files and folders to change"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "wait_on", "wait_on"
  end

  test do
    system bin/"wait_on", "-v"
    (testpath/"watched.txt").write "x"
    system bin/"wait_on", "-t", "0", testpath/"watched.txt"
    assert_match "can't open", shell_output("#{bin}/wait_on #{testpath}/missing 2>&1", 66)
    assert_match "usage: wait_on", shell_output("#{bin}/wait_on 2>&1", 64)
  end
end
