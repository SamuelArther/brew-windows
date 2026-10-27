class Mark < Formula
  desc "Sync your markdown files with Confluence pages"
  homepage "https://samizdat.dev"
  url "https://github.com/kovetskiy/mark/archive/refs/tags/v16.24.0.tar.gz"
  sha256 "c452660accedf43fc841d9227dc9db41e2274f0ed0ec087f93c45b8c155410d1"
  license "Apache-2.0"
  head "https://github.com/kovetskiy/mark.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "be86ada83704f55e2172f7e6b359b1b98b1d61ee53f5bcf1db52e0c35e31fc26"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: :goreleaser), "./cmd/mark"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mark --version")

    (testpath/"test.md").write <<~MARKDOWN
      # Hello Homebrew
    MARKDOWN

    touch testpath/"mark.toml"
    output = shell_output("#{bin}/mark --config mark.toml publish 2>&1", 1)
    assert_match "confluence base URL should be specified", output
  end
end
