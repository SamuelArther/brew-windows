class Gssh < Formula
  desc "SSH automation tool based on Groovy DSL"
  homepage "https://gradle-ssh-plugin.github.io/"
  url "https://github.com/int128/groovy-ssh/archive/refs/tags/2.12.0.tar.gz"
  sha256 "b2ce4ddc0d208e90ab4e986a1b1d759955783bdf3fe046f770eba98e6e6a13a9"
  license "Apache-2.0"
  head "https://github.com/int128/gradle-ssh-plugin.git", branch: "master"

  deprecate! date: "2025-05-25", because: :repo_archived
  disable! date: "2026-06-03", because: :repo_archived

  depends_on "gradle@8" => :build
  depends_on "openjdk@21"

  def install
    ENV["CIRCLE_TAG"] = version
    ENV["GROOVY_SSH_VERSION"] = version
    system "gradle", "shadowJar", "--no-daemon"
    libexec.install "#{"groovy-ssh/" if build.head?}cli/build/libs/gssh.jar"
    bin.write_jar_script libexec/"gssh.jar", "gssh", java_version: "21"
  end

  test do
    assert_match "groovy-ssh-#{version}", shell_output("#{bin}/gssh --version")
  end
end
