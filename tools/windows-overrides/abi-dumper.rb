class AbiDumper < Formula
  desc "Dump ABI of an ELF or PE object containing DWARF debug info"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "abi-dumper", "abi-dumper"
  end

  test do
    assert_match "ABI Dumper", shell_output("#{bin}/abi-dumper -v")
    # An object without DWARF is refused the way abi-dumper does.
    cp libexec/"wintools.exe", testpath/"nodebug.exe"
    assert_match "can't find debug info", shell_output("#{bin}/abi-dumper #{testpath}/nodebug.exe -o x.dump 2>&1", 2)
    (testpath/"test.c").write "int foo() { return 0; }\n"
    system ENV.cc, "-g", "-Og", "-shared", "test.c", "-o", testpath/"libtest.dll"
    system bin/"abi-dumper", testpath/"libtest.dll", "-o", "test.dump"
    assert_path_exists testpath/"test.dump"
    assert_match "'ShortName' => 'foo'", (testpath/"test.dump").read
  end
end
