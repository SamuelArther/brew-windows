class Checkpwn < Formula
  desc "Check Have I Been Pwned and see if it's time for you to change passwords"
  homepage "https://github.com/brycx/checkpwn"
  url "https://static.crates.io/crates/checkpwn/checkpwn-0.6.4.crate"
  sha256 "3cd332d710423463b159cc5e3e0af42d4d498e0d36eda3bae9e8c3c18fb11d44"
  license "MIT"
  head "https://github.com/brycx/checkpwn.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "fa49afa8dab081d64e615cf3e86c02657c35c5fba274db6fdd396c9f476a6c28"
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
    output = shell_output("#{bin}/checkpwn acc test@example.com 2>&1", 101)
    assert_match "Failed to read or parse the configuration file 'checkpwn.yml'", output
  end
end
