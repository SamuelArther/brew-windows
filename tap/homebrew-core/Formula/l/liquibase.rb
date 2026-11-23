class Liquibase < Formula
  desc "Library for database change tracking"
  homepage "https://www.liquibase.com/community"
  # NOTE: Do not bump to v5.0.0+ as license changed to FSL-1.1-ALv2
  url "https://github.com/liquibase/liquibase/releases/download/v4.33.0/liquibase-4.33.0.tar.gz"
  sha256 "689acfcdc97bad0d4c150d1efab9c851e251b398cb3d6326f75e8aafe40ed578"
  license "Apache-2.0"
  revision 1

  livecheck do
    url "https://github.com/liquibase/liquibase.git"
    regex(/^v?(4(?:\.\d+)+)$/i)
    strategy :github_releases
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "1d5ad735cf877b0991346defe37e9ec2246508db6f85fd82285b5248d146f51e"
  end

  depends_on "openjdk"

  def install
    rm(Dir["*.bat"])

    chmod 0755, "liquibase"
    libexec.install Dir["*"]
    bash_completion.install libexec/"lib/liquibase_autocomplete.sh" => "liquibase"
    zsh_completion.install libexec/"lib/liquibase_autocomplete.zsh" => "_liquibase"
    (bin/"liquibase").write_env_script libexec/"liquibase", Language::Java.overridable_java_home_env
  end

  def caveats
    <<~EOS
      You should set the environment variable LIQUIBASE_HOME to
        #{opt_libexec}
    EOS
  end

  test do
    system bin/"liquibase", "--version"
  end
end
