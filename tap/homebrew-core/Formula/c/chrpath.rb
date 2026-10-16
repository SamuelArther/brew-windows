class Chrpath < Formula
  desc "Tool to edit the rpath in ELF binaries"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "chrpath", "chrpath"
  end

  test do
    assert_match "chrpath version", shell_output("#{bin}/chrpath -v")
    cp test_fixtures("elf/hello_with_rpath"), testpath/"a.out"
    assert_match "a.out: RPATH=@@HOMEBREW_PREFIX@@/lib", shell_output("#{bin}/chrpath a.out")
    assert_match "a.out: new RPATH: /usr/lib/", shell_output("#{bin}/chrpath -r /usr/lib/ a.out")
    assert_match "a.out: RPATH converted to RUNPATH", shell_output("#{bin}/chrpath -c a.out")
    assert_match "a.out: RUNPATH=/usr/lib/", shell_output("#{bin}/chrpath a.out")
    system bin/"chrpath", "-d", "a.out"
    assert_match "a.out: no rpath or runpath tag found.", shell_output("#{bin}/chrpath a.out", 2)
  end
end
