class Checkpwn < Formula
  desc "Check Have I Been Pwned and see if it's time for you to change passwords"
  homepage "https://github.com/brycx/checkpwn"
  url "https://static.crates.io/crates/checkpwn/checkpwn-0.6.2.crate"
  sha256 "ab3ba2a2fe867307ae05121d24eef96527b25ca1ca8ef10d808b24e2e51c271e"
  license "MIT"
  head "https://github.com/brycx/checkpwn.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a60721fc48109bd965e380874c905f21b60dc0f16c2f7affe027c295dcf3ea24"
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
