class Fortitude < Formula
  desc "Fortran linter"
  homepage "https://fortitude.readthedocs.io/en/stable/"
  url "https://github.com/PlasmaFAIR/fortitude/releases/download/v0.9.2/fortitude-x86_64-pc-windows-msvc.zip"
  sha256 "5962a575701aca6555dcbd67b89aba6e894a8da99aa39cf1a4128fd2d23a690c"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/PlasmaFAIR/fortitude/archive/refs/tags/v0.9.2.tar.gz"
  end

  def install
    bin.install "fortitude.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/fortitude --version")

    (testpath/"test.f90").write <<~FORTRAN
      PROGRAM hello
        WRITE(*,'(A)') 'Hello World!'
      ENDPROGRAM
    FORTRAN

    output = shell_output("#{bin}/fortitude check #{testpath}/test.f90 --output-format concise 2>&1", 1)
    assert_match "fortitude: 1 files scanned.\nNumber of errors: 5", output
  end
end
