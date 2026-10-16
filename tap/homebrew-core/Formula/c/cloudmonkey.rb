class Cloudmonkey < Formula
  desc "Apache CloudStack CloudMonkey CLI"
  homepage "https://github.com/apache/cloudstack-cloudmonkey"
  url "https://github.com/apache/cloudstack-cloudmonkey/archive/refs/tags/6.6.0.tar.gz"
  sha256 "fdebc87604f8047d9b88ed03b6a9b50bf039242726e5e8e80b42e82fd7d326e7"
  license "Apache-2.0"
  head "https://github.com/apache/cloudstack-cloudmonkey.git", branch: "main"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "903fb21ad3e9f444bbfb7dd307f23a266275e4049c05849c98ce9a7801cd2575"
  end

  depends_on "go" => :build

  deny_network_access!

  def install
    ldflags = "-X main.GitSHA=homebrew -X main.BuildDate=#{time.iso8601}"
    system "go", "build", *std_go_args(ldflags:, tags: "release", output: bin/"cmk"), "cmk.go"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cmk -v")

    config_file = testpath/"cmk.ini"
    touch config_file

    # `set` writes through the INI config layer without any network calls;
    # this exercises config init, profile defaults, and key updates.
    system bin/"cmk", "-c", config_file, "set", "asyncblock", "false"
    assert_path_exists config_file
    assert_match(/^asyncblock\s*=\s*false$/, config_file.read)
  end
end
