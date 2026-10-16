class Sleepwatcher < Formula
  desc "Monitors sleep, wakeup, and idleness of the PC"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "sleepwatcher", "sleepwatcher"
  end

  test do
    assert_match(/sleepwatcher \d+\.\d+/, shell_output("#{bin}/sleepwatcher -v", 2))
    assert_match(/\A\d+\z/, shell_output("#{bin}/sleepwatcher -g").strip)
  end
end
