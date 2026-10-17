class Sleek < Formula
  desc "CLI tool for formatting SQL"
  homepage "https://github.com/nrempel/sleek"
  url "https://github.com/nrempel/sleek/archive/refs/tags/v0.5.0.tar.gz"
  sha256 "fcb589fdc5ece8c050883ff0b56aec6bd25e2e4d6e77a1d52e870535d66fdf67"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "f1310d4dc06ea9ff96660aaeacb1ac7f8e6fd7be7c3e9ecf2526a9ebe0843817"
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
    assert_match version.to_s, shell_output("#{bin}/sleek --version")

    (testpath/"test.sql").write <<~SQL
      SELECT * from foo WHERE bar = 'quux';
    SQL
    system bin/"sleek", testpath/"test.sql"
  end
end
