class LdFindCodeRefs < Formula
  desc "Build tool for sending feature flag code references to LaunchDarkly"
  homepage "https://launchdarkly.com"
  url "https://github.com/launchdarkly/ld-find-code-refs/releases/download/v2.18.3/ld-find-code-refs_2.18.3_windows_amd64.tar.gz"
  sha256 "bc88ba6d3b4244b5531dbe5b6415f778ab2e06812f671c3422df67a8a98039bf"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/launchdarkly/ld-find-code-refs/archive/refs/tags/v2.18.3.tar.gz"
  end

  def install
    bin.install "ld-find-code-refs.exe"
  end

  test do
    system "git", "init"
    (testpath/"README").write "Testing"
    (testpath/".gitignore").write "Library"
    system "git", "add", "README", ".gitignore"
    system "git", "commit", "-m", "Initial commit"

    assert_match "could not retrieve flag key",
      shell_output("#{bin}/ld-find-code-refs --dryRun \
                   --ignoreServiceErrors -t=xx -p=test -r=test -d=. 2>&1", 1)
  end
end
