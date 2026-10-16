class Bindfs < Formula
  desc "Mirror a folder at another location"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
