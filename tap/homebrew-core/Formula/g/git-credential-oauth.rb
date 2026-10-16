class GitCredentialOauth < Formula
  desc "Git credential helper that authenticates in browser using OAuth"
  homepage "https://github.com/hickford/git-credential-oauth"
  url "https://github.com/hickford/git-credential-oauth/releases/download/v0.17.2/git-credential-oauth_0.17.2_windows_amd64.zip"
  sha256 "268a95e29fa8b56f1f42d65bf4a7465aefb74e9b671dd656129f6207151e0aae"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/hickford/git-credential-oauth/archive/refs/tags/v0.17.2.tar.gz"
  end

  def install
    bin.install "git-credential-oauth.exe"
  end

  test do
    assert_match "git-credential-oauth #{version}", shell_output("#{bin}/git-credential-oauth -verbose 2>&1", 2)
  end
end
