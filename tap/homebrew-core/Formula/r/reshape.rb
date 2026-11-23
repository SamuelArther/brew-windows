class Reshape < Formula
  desc "Easy-to-use, zero-downtime schema migration tool for Postgres"
  homepage "https://github.com/fabianlindfors/reshape"
  url "https://static.crates.io/crates/reshape/reshape-0.11.2.crate"
  sha256 "17c0680e957ab12c43da0c55741ca7c1dd1003b20216ca7f84ca1d3aff57391c"
  license "MIT"
  head "https://github.com/fabianlindfors/reshape.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "415bbe01d2c86c9c9850b0607f21b27819c27b5d5f747c0687dfcdf283f72f09"
  end

  depends_on "pkgconf" => :build
  depends_on "rust" => :build

  uses_from_macos "llvm" => :build # for libclang to build pg_query

  on_linux do
    depends_on "openssl@4"
  end

  allow_network_access! :test

  def openssl = deps("openssl").first&.name

  def fetch
    system "cargo", "generate-lockfile" if build.head?
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    ENV["OPENSSL_DIR"] = formula_opt_prefix(openssl) if OS.linux?
    system "cargo", "install", *std_cargo_args
  end

  test do
    (testpath/"migrations/test.toml").write <<~TOML
      [[actions]]
      type = "create_table"
      name = "users"
      primary_key = ["id"]

        [[actions.columns]]
        name = "id"
        type = "INTEGER"
        generated = "ALWAYS AS IDENTITY"

        [[actions.columns]]
        name = "name"
        type = "TEXT"
    TOML

    assert_match "SET search_path TO migration_test",
      shell_output("#{bin}/reshape generate-schema-query")

    assert_match "Error: error connecting to server",
      shell_output("#{bin}/reshape migrate 2>&1", 1)
  end
end
