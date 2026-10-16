class Launch4j < Formula
  desc "Cross-platform Java executable wrapper"
  homepage "https://launch4j.sourceforge.net/"
  url "https://git.code.sf.net/p/launch4j/git.git",
      tag:      "Release_launch4j-3_50",
      revision: "0b6f1c1ffe4e83dc47fa866a733dbf0125237937"
  license all_of: ["BSD-3-Clause", "MIT"]

  livecheck do
    url :stable
    regex(/^(?:Release_launch4j[._-])?v?(\d+(?:[._]\d+)+)$/i)
    strategy :git do |tags, regex|
      tags.map { |tag| tag[regex, 1]&.tr("_", ".") }
    end
  end

  no_autobump! because: :incompatible_version_format

  # https://docs.brew.sh/Support-Tiers#future-macos-support
  deprecate! date: "2025-09-25", because: :unsupported
  disable! date: "2026-09-25", because: :unsupported

  depends_on "ant" => :build
  depends_on arch: :x86_64
  # Installs a pre-built `ld` and `windres` file with linkage to zlib
  depends_on :macos
  depends_on "openjdk"

  def install
    system "ant", "compile"
    system "ant", "jar"
    libexec.install Dir["*"] - ["src", "web"]
    bin.write_jar_script libexec/"launch4j.jar", "launch4j"
  end

  test do
    system bin/"launch4j", "--version"
  end
end
