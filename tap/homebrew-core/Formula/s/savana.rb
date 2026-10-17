class Savana < Formula
  desc "Transactional workspaces for SVN"
  homepage "https://github.com/codehaus/savana"
  url "https://search.maven.org/remotecontent?filepath=org/codehaus/savana/1.2/savana-1.2-install.tar.gz"
  sha256 "608242a0399be44f41ff324d40e82104b3c62908bc35177f433dcfc5b0c9bf55"
  license "LGPL-3.0-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "086ebf0e36fa5dcd17953ef27fe63109f917c05e9167406a06c6344a2d229a76"
  end

  depends_on "openjdk"

  def install
    # Remove Windows files
    rm_r(Dir["bin/*.{bat,cmd}"])

    prefix.install %w[COPYING COPYING.LESSER licenses svn-hooks]

    # lib/* and logging.properties are loaded relative to bin
    prefix.install "bin"
    libexec.install %w[lib logging.properties]
    bin.env_script_all_files libexec/"bin", JAVA_HOME: formula_opt_prefix("openjdk")

    bash_completion.install "etc/bash_completion" => "sav"
  end

  test do
    system bin/"sav", "help"
  end
end
