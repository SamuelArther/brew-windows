class Bindfs < Formula
  desc "Mirror a folder at another location"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "bindfs", "bindfs"
  end

  test do
    assert_match "bindfs", shell_output("#{bin}/bindfs -V")
    (testpath/"src").mkpath
    (testpath/"src/f.txt").write "hi"
    system bin/"bindfs", testpath/"src", testpath/"mnt"
    assert_equal "hi", (testpath/"mnt/f.txt").read
    system bin/"bindfs", "-u", testpath/"mnt"
    refute_path_exists testpath/"mnt"
    assert_path_exists testpath/"src/f.txt"
  end
end
