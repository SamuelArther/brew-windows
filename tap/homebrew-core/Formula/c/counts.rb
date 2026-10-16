class Counts < Formula
  desc "Tool for ad hoc profiling"
  homepage "https://github.com/nnethercote/counts"
  url "https://github.com/nnethercote/counts/archive/refs/tags/1.0.7.tar.gz"
  sha256 "a5685538819838ba2fba0b78d11b5d80e37753b9015735f71f0c2065442fe9d8"
  license "Unlicense"
  head "https://github.com/nnethercote/counts.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "4e7b516e96f71b94e6ff08257cbfe8dcc2247344f04f19a85796be28b2f71529"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    (testpath/"test.txt").write <<~EOS
      a 1
      b 2
      c 3
      d 4
      d 4
      c 3
      c 3
      d 4
      b 2
      d 4
    EOS

    output = shell_output("#{bin}/counts test.txt")
    expected = <<~EOS
      10 counts
      (  1)        4 (40.0%, 40.0%): d 4
      (  2)        3 (30.0%, 70.0%): c 3
      (  3)        2 (20.0%, 90.0%): b 2
      (  4)        1 (10.0%,100.0%): a 1
    EOS

    assert_equal expected, output

    assert_match "counts-#{version}", shell_output("#{bin}/counts --version")
  end
end
