class Elfutils < Formula
  desc "Utilities for handling ELF objects and their DWARF debug info"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "eu-readelf", "eu-readelf"
    windows_exe "libexec/wintools.exe", "eu-nm", "eu-nm"
    windows_exe "libexec/wintools.exe", "eu-size", "eu-size"
    windows_exe "libexec/wintools.exe", "eu-strings", "eu-strings"
    windows_exe "libexec/wintools.exe", "eu-elfclassify", "eu-elfclassify"
    windows_exe "libexec/wintools.exe", "eu-elfcmp", "eu-elfcmp"
    windows_exe "libexec/wintools.exe", "eu-addr2line", "eu-addr2line"
    windows_exe "libexec/wintools.exe", "eu-srcfiles", "eu-srcfiles"
    windows_exe "libexec/wintools.exe", "eu-strip", "eu-strip"
  end

  test do
    cp test_fixtures("elf/hello"), testpath
    assert_match "puts@GLIBC_2.2.5", shell_output("#{bin}/eu-nm -D hello")
    assert_match "AMD x86-64", shell_output("#{bin}/eu-readelf -h hello")
    assert_match "Build ID: 38b6fecd1314a6c95646f32024750ffc095298e0", shell_output("#{bin}/eu-readelf -n hello")
    assert_match "Shared library: [libc.so.6]", shell_output("#{bin}/eu-readelf -d hello")
    assert_match "GLIBC_2.2.5", shell_output("#{bin}/eu-strings hello")
    assert_match(/\d+\s+\d+\s+\d+\s+\d+\s+\h+\s+hello/, shell_output("#{bin}/eu-size hello"))
    assert_equal "hello\n", shell_output("#{bin}/eu-elfclassify --program --print hello")
    system bin/"eu-elfcmp", "hello", "hello"
    system bin/"eu-strip", "-o", "hello.stripped", "hello"
    system bin/"eu-elfcmp", "hello", "hello.stripped"
  end
end
