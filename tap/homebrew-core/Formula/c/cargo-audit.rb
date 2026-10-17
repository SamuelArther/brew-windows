class CargoAudit < Formula
  desc "Audit Cargo.lock files for crates with security vulnerabilities"
  homepage "https://rustsec.org/"
  url "https://github.com/rustsec/rustsec/archive/refs/tags/cargo-audit/v0.22.2.tar.gz"
  sha256 "85c368a4d166b2cc4972108d50abc5fad605013b65098929a06122439488beb5"
  license any_of: ["Apache-2.0", "MIT"]
  head "https://github.com/rustsec/rustsec.git", branch: "main"

  livecheck do
    url :stable
    regex(%r{^cargo-audit/v?(\d+(?:\.\d+)+)$}i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "7c8beb04f66ad7cfddcf1bd63c83f083c8b0083febae1ad6f803c76d8bc31544"
  end

  depends_on "rust" => :build

  allow_network_access! :test

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "cargo-audit")
    # test cargo-audit
    pkgshare.install "cargo-audit/tests/support/base64_vuln"
  end

  test do
    output = shell_output("#{bin}/cargo-audit audit 2>&1", 2)
    assert_path_exists HOMEBREW_CACHE/"cargo_cache/advisory-db"
    assert_match "not found: Couldn't load Cargo.lock", output

    cp_r "#{pkgshare}/base64_vuln/.", testpath
    assert_match "error: 1 vulnerability found!", shell_output("#{bin}/cargo-audit audit 2>&1", 1)
  end
end
