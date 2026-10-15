class Ruff < Formula
  desc "Extremely fast Python linter, written in Rust"
  homepage "https://docs.astral.sh/ruff/"
  url "https://github.com/astral-sh/ruff/releases/download/0.17.0/ruff-x86_64-pc-windows-msvc.zip"
  sha256 "bc831eaa01912323099d7bc185981bf6bb1960931350bd3ad6a82f7a59a46c3b"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "ruff.exe"
  end

  test do
    (testpath/"test.py").write <<~PYTHON
      import os
    PYTHON
    assert_match "F401", shell_output("#{bin}/ruff check #{testpath}/test.py", 1)
  end
end
