class Lispkit < Formula
  desc "R7RS Scheme for extension and scripting (Gauche's official Windows build)"
  homepage "https://practical-scheme.net/gauche/"
  url "https://github.com/shirok/Gauche/releases/download/release0_9_15/Gauche-mingw-0.9.15-64bit.msi"
  version "0.9.15"
  sha256 "3e0f6c2139fc4c9daec99402bcdf3ebc33114d6167538b28994246227b174f54"
  license "BSD-3-Clause"

  # LispKit is an R7RS Scheme written in Swift on top of Foundation and Apple's graphics frameworks,
  # so it only builds for macOS and iOS. Its Windows counterpart is Gauche, an R7RS Scheme with an
  # official 64-bit Windows build (the MSI is read as an archive; no installer runs). `lispkit`
  # starts gosh in R7RS mode (-r7): `lispkit script.scm`, `lispkit -b < file`, or a REPL.
  # LispKit's own (lispkit ...) libraries are not available; R7RS and SRFI libraries are.

  livecheck do
    url "https://github.com/shirok/Gauche"
    regex(/^release[._-]?v?(\d+(?:[._]\d+)+)$/i)
  end

  def install
    install_tree
    windows_exe "libexec/bin/gosh.exe", "lispkit", "-r7"
  end

  test do
    assert_equal "6", pipe_output("#{bin}/lispkit -b", "(display (+ 1 2 3))").strip
    (testpath/"hello.scm").write <<~SCHEME
      (import (scheme base) (scheme write))
      (define (square x) (* x x))
      (display (map square '(1 2 3)))
    SCHEME
    assert_equal "(1 4 9)", shell_output("#{bin}/lispkit #{testpath}/hello.scm").strip
  end
end
