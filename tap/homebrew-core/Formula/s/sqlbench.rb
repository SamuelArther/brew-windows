class Sqlbench < Formula
  desc "Measures and compares the execution time of one or more SQL queries"
  homepage "https://github.com/felixge/sqlbench"
  url "https://github.com/felixge/sqlbench/releases/download/v1.1.0/sqlbench_1.1.0_windows_amd64.zip"
  sha256 "0323409ee4c723cdbe292a8eacf92b907de5ddcd551318c825330aee1956e9fa"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/felixge/sqlbench/archive/refs/tags/v1.1.0.tar.gz"
  end

  def install
    bin.install "sqlbench.exe"
  end

  test do
    system bin/"sqlbench", "--version"
  end
end
