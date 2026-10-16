class ObjcRun < Formula
  desc "Use Objective-C files for shell script-like tasks"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "objc-run", "objc-run"
  end

  def caveats
    <<~EOS
      objc-run compiles with clang against GNUstep's Foundation. Install both, e.g.
        brew install llvm gnustep-base
      or point GNUSTEP_ROOT at an existing GNUstep (include/Foundation, lib, bin).
    EOS
  end

  test do
    assert_match "objc-run usage", shell_output("#{bin}/objc-run 2>&1", 1)
    (testpath/"hello.m").write "#!/usr/bin/env objc-run\nint main(void) { return 0; }\n"
    touch testpath/"hello"
    assert_match "file already exists", shell_output("#{bin}/objc-run #{testpath}/hello.m 2>&1", 1)
    (testpath/"pods.m").write "/*\npodfile-start\npod 'Barista'\npodfile-end\n*/\n"
    assert_match "CocoaPods", shell_output("#{bin}/objc-run #{testpath}/pods.m 2>&1", 1)
    assert_match "GNUSTEP_ROOT", shell_output("#{bin}/objc-run --help")
  end
end
