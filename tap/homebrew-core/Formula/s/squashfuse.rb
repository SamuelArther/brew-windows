class Squashfuse < Formula
  desc "Unpack squashfs archives into folders"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "squashfuse", "squashfuse"
  end

  test do
    assert_match "squashfuse", shell_output("#{bin}/squashfuse --version")
    (testpath/"bad.img").write "not a squashfs image, just text"
    (testpath/"mnt").mkpath
    assert_match "not a squashfs image", shell_output("#{bin}/squashfuse #{testpath}/bad.img #{testpath}/mnt 2>&1", 1)
  end
end
