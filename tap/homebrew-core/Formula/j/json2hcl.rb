class Json2hcl < Formula
  desc "Convert JSON to HCL, and vice versa"
  homepage "https://github.com/kvz/json2hcl"
  url "https://github.com/kvz/json2hcl/archive/refs/tags/v0.2.0.tar.gz"
  sha256 "ac10155d2d86a196a97e9cfb98e7a66f0b0505dee8904bbd3e32979370b81f34"
  license "MIT"
  head "https://github.com/kvz/json2hcl.git", branch: "master"

  deprecate! date: "2025-10-26", because: :repo_archived
  disable! date: "2026-10-26", because: :repo_archived

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "af4fc0ef699c749a99cc545520f1cb529201b4f051cd6a7e9c38b96d9529eb71"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.Version=#{version}")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/json2hcl -version")

    json = <<~JSON
      {
        "hello": "world"
      }
    JSON

    assert_equal "\"hello\" = \"world\"", pipe_output(bin/"json2hcl", json, 0)

    hcl = <<~HCL
      hello = "world"
    HCL

    assert_equal json.chomp, pipe_output("#{bin}/json2hcl -reverse", hcl, 0).chomp
  end
end
