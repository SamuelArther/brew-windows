class Aha < Formula
  desc "ANSI HTML adapter"
  homepage "https://github.com/theZiz/aha"
  url "https://github.com/theZiz/aha/archive/refs/tags/0.5.1.tar.gz"
  sha256 "6aea13487f6b5c3e453a447a67345f8095282f5acd97344466816b05ebd0b3b1"
  license any_of: ["LGPL-2.0-or-later", "MPL-1.1"]
  compatibility_version 1
  head "https://github.com/theZiz/aha.git", branch: "master"

  deny_network_access!

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "62dbaeb00678d9ad905bd410bc3a2f263dd0e68f89477a8d57a74b7aff74d03b"
  end

  def install
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    out = pipe_output(bin/"aha", "[35mrain[34mpill[00m", 0)
    assert_match(/color:purple;">rain.*color:blue;">pill/, out)
  end
end
