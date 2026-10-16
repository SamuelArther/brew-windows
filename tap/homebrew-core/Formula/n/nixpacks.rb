class Nixpacks < Formula
  desc "App source + Nix packages + Docker = Image"
  homepage "https://nixpacks.com/docs/getting-started"
  url "https://github.com/railwayapp/nixpacks/archive/refs/tags/v1.41.0.tar.gz"
  sha256 "38b1c8a739a7585eb4aff071bd0e8f067ef35eca6af32acc3a2dec0799a78a83"
  license "MIT"
  head "https://github.com/railwayapp/nixpacks.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "2a88e59769e7516e80daaacb40121ce35a97e9f6014527fc56520be7964f4f9d"
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
    output = shell_output("#{bin}/nixpacks build #{testpath} --name test", 1)
    assert_match "Nixpacks was unable to generate a build plan for this app", output

    assert_equal "nixpacks #{version}", shell_output("#{bin}/nixpacks -V").chomp
  end
end
