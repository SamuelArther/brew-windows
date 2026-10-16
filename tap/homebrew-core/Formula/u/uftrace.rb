class Uftrace < Formula
  desc "Function graph tracer for C/C++/Rust"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "uftrace", "uftrace"
  end

  def caveats
    <<~EOS
      uftrace patches functions through the Windows debugging API and finds them in the program's
      PDB or COFF symbol table (MinGW builds keep one unless stripped). Timings include the debugger's
      round trip per event.
    EOS
  end

  test do
    assert_match "uftrace v0.20", shell_output("#{bin}/uftrace --version")
    (testpath/"hello.c").write <<~C
      #include <stdio.h>
      __attribute__((noinline)) int leaf(int x) { return x * 2; }
      int main(void) { printf("%d\\n", leaf(21)); return 0; }
    C
    system ENV.cc, "hello.c", "-o", "hello.exe"
    out = shell_output("#{bin}/uftrace -A . -R . -P main -P leaf ./hello.exe")
    assert_match "| main(", out
    assert_match "leaf(21", out
    assert_match "} = 0; /* main */", out
    system bin/"uftrace", "record", "-P", "leaf", "./hello.exe"
    assert_match "leaf", shell_output("#{bin}/uftrace report")
    assert_match "program path", shell_output("#{bin}/uftrace info")
  end
end
