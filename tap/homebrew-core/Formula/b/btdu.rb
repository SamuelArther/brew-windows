class Btdu < Formula
  desc "Sampling disk usage profiler for NTFS"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "btdu", "btdu"
  end

  test do
    assert_match "Fatal error: No path specified", shell_output("#{bin}/btdu 2>&1", 1)
    assert_match "btdu v", shell_output("#{bin}/btdu --version")
    # Sampling reads the volume, which needs administrator rights (GitHub's runners have them).
    output = shell_output("#{bin}/btdu --headless --max-samples 64 --seed 1 --export #{testpath}/btdu.json C:/")
    assert_match "64 samples", output
    assert_match "\"samples\":64", (testpath/"btdu.json").read
  end
end
