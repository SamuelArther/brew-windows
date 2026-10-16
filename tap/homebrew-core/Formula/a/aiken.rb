class Aiken < Formula
  desc "Modern smart contract platform for Cardano"
  homepage "https://aiken-lang.org/"
  url "https://github.com/aiken-lang/aiken/archive/refs/tags/v1.1.24.tar.gz"
  sha256 "0508470ea01156ef7e275d97f12ceadc99d952897d4cb7454f790b15bfe31d0e"
  license "Apache-2.0"
  head "https://github.com/aiken-lang/aiken.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "50784a28b12ff9c1edbbda0b5e7e72731c814ceeff6f1298757916b03970b73a"
  end

  depends_on "pkgconf" => :build
  depends_on "rust" => :build

  on_linux do
    depends_on "openssl@4"
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", "--locked", "--target", "host-tuple"
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "crates/aiken")

    # aiken has no `completion` subcommand on Windows (cfg(not(target_os = "windows"))).
    generate_completions_from_executable(bin/"aiken", "completion") unless OS.windows?
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/aiken --version")

    system bin/"aiken", "new", "brewtest/hello"
    assert_path_exists testpath/"hello/README.md"
    assert_match "brewtest/hello", (testpath/"hello/aiken.toml").read
  end
end
