class Jsonfmt < Formula
  desc "Like gofmt, but for JSON files"
  homepage "https://github.com/caarlos0/jsonfmt"
  url "https://github.com/caarlos0/jsonfmt/archive/refs/tags/v0.5.2.tar.gz"
  sha256 "07494aaec99d4c8cd7f0cea1bacd32e1d20321e541848aa6aafa64d64d89c9ab"
  license "MIT"
  head "https://github.com/caarlos0/jsonfmt.git", branch: "master"

  deprecate! date: "2026-07-17", because: :repo_archived
  disable! date: "2027-01-17", because: :repo_archived

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "77b842529af5f97d84a913b6dc6c19bedbef3fb1837d200ad610b745c3c5c02e"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.version=#{version}")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/jsonfmt --version")

    (testpath/"test.json").write <<~JSON
      {"foo":"bar","baz":{"qux":"quux","corge":"grault"}}
    JSON

    expected_output = <<~JSON
      {
        "foo": "bar",
        "baz": {
          "qux": "quux",
          "corge": "grault"
        }
      }
    JSON

    system bin/"jsonfmt", "--write", testpath/"test.json"
    assert_equal expected_output, (testpath/"test.json").read
  end
end
