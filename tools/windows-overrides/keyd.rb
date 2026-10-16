class Keyd < Formula
  desc "Key remapping daemon"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "keyd", "keyd"
  end

  test do
    assert_match(/keyd v\d+\.\d+/, shell_output("#{bin}/keyd --version"))
    (testpath/"test.conf").write <<~CONF
      [ids]
      *

      [main]
      capslock = overload(control, esc)
    CONF
    assert_match "No errors found", shell_output("#{bin}/keyd check #{testpath}/test.conf")
    # simulate prints the remapped events without sending any input
    assert_match "-> esc down", shell_output("#{bin}/keyd simulate #{testpath}/test.conf capslock")
    assert_match "-> leftcontrol down", shell_output("#{bin}/keyd simulate #{testpath}/test.conf capslock:down a capslock:up")
  end
end
