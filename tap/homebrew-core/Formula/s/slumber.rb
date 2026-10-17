class Slumber < Formula
  desc "Terminal-based HTTP/REST client"
  homepage "https://slumber.lucaspickering.me/"
  url "https://github.com/LucasPickering/slumber/archive/refs/tags/v5.4.0.tar.gz"
  sha256 "f781bf8f0a369641d0b6c8e19336be3cb26ddd44c06a008edc0aad6d7b927442"
  license "MIT"
  head "https://github.com/LucasPickering/slumber.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "e8e67b8e762727a3c81a0085d0fc9204097432f0a34824446b7197a4d35de104"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
    generate_completions_from_executable(bin/"slumber", shell_parameter_format: :clap)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/slumber --version")

    system bin/"slumber", "new"
    assert_match <<~YAML, (testpath/"slumber.yml").read
      profiles:
        example:
          name: Example Profile
          data:
            host: https://my-host
    YAML
  end
end
