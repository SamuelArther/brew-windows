class SqlxCli < Formula
  desc "Command-line utility for SQLx, the Rust SQL toolkit"
  homepage "https://github.com/transact-rs/sqlx"
  url "https://static.crates.io/crates/sqlx-cli/sqlx-cli-0.9.0.crate"
  sha256 "93ef3857a4a0b48fcbf536b77a9122a35c7631686f2ccfbc75e616335771e8d0"
  license any_of: ["Apache-2.0", "MIT"]

  bottle do
    sha256 cellar: :any, x86_64_windows: "d2147ac902dcd5f89df53062571db8ba39af84780e34e21dec3c04e81298c42a"
  end

  depends_on "pkgconf" => :build
  depends_on "rust" => :build

  on_linux do
    depends_on "openssl@4"
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args

    generate_completions_from_executable(bin/"sqlx", "completions")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/sqlx --version")

    ENV["DATABASE_URL"] = "postgres://postgres@localhost/my_database"
    output = shell_output("#{bin}/sqlx migrate info 2>&1", 1)
    assert_match "error: while resolving migrations: error canonicalizing path migrations", output
  end
end
