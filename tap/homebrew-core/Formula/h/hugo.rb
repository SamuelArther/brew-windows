class Hugo < Formula
  desc "Configurable static site generator"
  homepage "https://gohugo.io/"
  version "0.168.0"
  url "https://github.com/gohugoio/hugo/releases/download/v0.168.0/hugo_extended_withdeploy_0.168.0_windows-amd64.zip"
  sha256 "b5c1a2c0d58083de6c06b112eef9f756ae72f4375100bf6217baeb769d4eef4c"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  # Homebrew builds Hugo with the extended,withdeploy tags; this is the matching official build.

  def install
    bin.install "hugo.exe"
  end

  test do
    site = testpath/"hops-yeast-malt-water"
    system bin/"hugo", "new", "site", site
    assert_path_exists site/"hugo.toml"

    assert_match version.to_s, shell_output("#{bin}/hugo version")
  end
end
