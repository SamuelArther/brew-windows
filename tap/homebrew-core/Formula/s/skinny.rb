class Skinny < Formula
  desc "Full-stack web app framework in Scala"
  homepage "https://skinny-framework.github.io"
  url "https://github.com/skinny-framework/skinny-framework/releases/download/4.0.1/skinny-4.0.1.tar.gz"
  sha256 "2382ba97f799bfc772ee79b2c084c63a1278ddd89de8dacd4ba6433f41294812"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "112c0f91e5d48d35227d1170dc243c11c705eaa5e5ea6f46679dc62e5210ca9c"
  end

  depends_on "openjdk"

  def install
    inreplace %w[skinny skinny-blank-app/skinny], "/usr/local", HOMEBREW_PREFIX
    libexec.install Dir["*"]

    skinny_env = Language::Java.overridable_java_home_env
    skinny_env[:PATH] = "#{bin}:${PATH}"
    skinny_env[:PREFIX] = libexec
    (bin/"skinny").write_env_script libexec/"skinny", skinny_env
  end

  test do
    system bin/"skinny", "new", "myapp"
  end
end
