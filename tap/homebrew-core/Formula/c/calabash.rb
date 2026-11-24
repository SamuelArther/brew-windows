class Calabash < Formula
  desc "XProc (XML Pipeline Language) implementation"
  homepage "https://xmlcalabash.com/"
  url "https://github.com/ndw/xmlcalabash1/releases/download/1.5.7-120/xmlcalabash-1.5.7-120.zip"
  sha256 "40a932910f36e78b445bd756acb405155d39b98541091298c0cf4971895cb8c3"
  license any_of: ["GPL-2.0-only", "CDDL-1.0"]

  # According to ndw/xmlcalabash1#342, each release comes in "flavours" that
  # target different Saxon versions (e.g. 1.5.4-110 targets Saxon 11.x).
  # The "latest" release on GitHub may not target the same version as our
  # `saxon` formula, so we have to check multiple releases to find the newest
  # applicable version.
  livecheck do
    url :stable
    regex(/^v?(\d+(?:[.-]\d+)+)$/i)
    strategy :github_releases do |json, regex|
      saxon_major = Formula["saxon"].version.major
      saxon_suffix = "-#{saxon_major}0"

      json.map do |release|
        next if release["draft"] || release["prerelease"]

        match = release["tag_name"]&.match(regex)
        next if match.blank?

        match[1] if match[1].end_with?(saxon_suffix)
      end
    end
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "f0f18ab71dc7111ffee8c919730e1f010c1b4eaf9a2004ad9cf5a7c28237a686"
  end

  depends_on "openjdk"
  depends_on "saxon"

  deny_network_access!

  def install
    libexec.install Dir["*"]
    bin.write_jar_script libexec/"xmlcalabash-#{version}.jar", "calabash", "-Xmx1024m"
  end

  test do
    # This small XML pipeline (*.xpl) that comes with Calabash
    # is basically its equivalent "Hello World" program.
    system bin/"calabash", "#{libexec}/xpl/pipe.xpl"
  end
end
