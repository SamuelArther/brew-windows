class Ipapatch < Formula
  desc "CLI tool to patch iOS IPA files and their plugins"
  homepage "https://github.com/asdfzxcvbn/ipapatch"
  url "https://github.com/asdfzxcvbn/ipapatch/archive/refs/tags/v2.1.3.tar.gz"
  sha256 "e7593e9d7cb77cdebce80f76129050cae984a9a41c4bfa12b0b5eec8dafa6f3a"
  license "MIT"
  head "https://github.com/asdfzxcvbn/ipapatch.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "7a23b156d3c340a5c1a68931e0e6ac20e09c6dd117ddaaf3c75ffed863b780d2"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    (testpath/"fake.ipa").write "not a real ipa"
    output = shell_output("#{bin}/ipapatch --input #{testpath}/fake.ipa --inplace --noconfirm 2>&1", 1)
    assert_match "not a valid zip file", output
  end
end
