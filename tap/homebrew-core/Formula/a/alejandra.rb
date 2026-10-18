class Alejandra < Formula
  desc "Command-line tool for formatting Nix Code"
  homepage "https://alejandra.kamadorueda.com/"
  url "https://github.com/kamadorueda/alejandra/archive/refs/tags/4.1.0.tar.gz"
  sha256 "cf74acd0bbbaa166abed6d155f3a6206392d715336e74f9bb5fe05d874de183b"
  license "Unlicense"
  head "https://github.com/kamadorueda/alejandra.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "c772407f7b6f99b3bbcb83e4b98764a8fcb3655234acfdb4e0b1a2ce3b5b4bc1"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "src/alejandra_cli")
  end

  test do
    assert_equal "Alejandra #{version}", shell_output("#{bin}/alejandra --version").chomp

    ENV["LC_ALL"] = "en_US.UTF-8"
    input_nix = "{description=\"Demo\";outputs={self}:{};}"
    output_nix = "{\n  description = \"Demo\";\n  outputs = {self}: {};\n}"

    (testpath/"alejandra_test.nix").write input_nix
    system bin/"alejandra", "alejandra_test.nix"
    assert_equal output_nix, (testpath/"alejandra_test.nix").read.strip
  end
end
