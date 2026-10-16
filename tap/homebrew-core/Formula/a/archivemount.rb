class Archivemount < Formula
  desc "Open archives as folders and write changes back"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "archivemount", "archivemount"
  end

  test do
    assert_match "archivemount", shell_output("#{bin}/archivemount --version")
    (testpath/"src").mkpath
    (testpath/"src/a.txt").write "one"
    system "C:/Windows/System32/tar.exe", "-a", "-cf", testpath/"arc.zip", "-C", testpath/"src", "a.txt"
    (testpath/"mnt").mkpath
    system bin/"archivemount", testpath/"arc.zip", testpath/"mnt"
    assert_equal "one", (testpath/"mnt/a.txt").read
    (testpath/"mnt/b.txt").write "two"
    system bin/"archivemount", "-u", testpath/"mnt"
    refute_path_exists testpath/"mnt/a.txt"
    assert_path_exists testpath/"arc.zip.orig"
    assert_match "b.txt", shell_output("C:/Windows/System32/tar.exe -tf #{testpath}/arc.zip")
  end
end
