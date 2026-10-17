class Repl < Formula
  desc "Wrap non-interactive programs with a REPL"
  homepage "http://defunkt.io/repl/"
  url "https://github.com/defunkt/repl/archive/refs/tags/v1.0.0.tar.gz"
  sha256 "d0542404f03159b0d6eb22a1aa4a509714c87c8594fca5121c578d50d950307d"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "fe1f259013b78eacecdae7a93424a6201775a2f097bf65065f685262c7478d4e"
  end

  conflicts_with "nmh", because: "both install `repl` binaries"

  def install
    # File.exists? is gone since Ruby 3.2; the ruby that runs the script on Windows is current.
    inreplace "bin/repl", "File.exists?", "File.exist?"
    bin.install "bin/repl"
    man1.install "man/repl.1"
  end

  test do
    pipe_output("#{bin}/repl git", "init", 0)
  end
end
