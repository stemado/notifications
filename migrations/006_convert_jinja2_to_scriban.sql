-- ============================================================================
-- Syntax Conversion: Jinja2/Ninja -> Scriban/Liquid
-- ============================================================================
-- Run this AFTER importing templates from MySQL to convert template syntax
-- for compatibility with the .NET Scriban rendering engine.
-- ============================================================================

-- Convert {% elif %} to {% elsif %}
UPDATE email_templates
SET html_content = REGEXP_REPLACE(html_content, '\{%\s*elif\s+', '{% elsif ', 'g'),
    text_content = REGEXP_REPLACE(text_content, '\{%\s*elif\s+', '{% elsif ', 'g'),
    subject = REGEXP_REPLACE(subject, '\{%\s*elif\s+', '{% elsif ', 'g'),
    updated_at = NOW()
WHERE html_content LIKE '%{%_elif_%'
   OR text_content LIKE '%{%_elif_%'
   OR subject LIKE '%{%_elif_%';

-- Convert {% else if %} to {% elsif %}
UPDATE email_templates
SET html_content = REGEXP_REPLACE(html_content, '\{%\s*else\s+if\s+', '{% elsif ', 'g'),
    text_content = REGEXP_REPLACE(text_content, '\{%\s*else\s+if\s+', '{% elsif ', 'g'),
    updated_at = NOW()
WHERE html_content LIKE '%{%_else if_%'
   OR text_content LIKE '%{%_else if_%';

-- Convert {% set var = %} to {% assign var = %}
UPDATE email_templates
SET html_content = REGEXP_REPLACE(html_content, '\{%\s*set\s+', '{% assign ', 'g'),
    text_content = REGEXP_REPLACE(text_content, '\{%\s*set\s+', '{% assign ', 'g'),
    updated_at = NOW()
WHERE html_content LIKE '%{%_set_%'
   OR text_content LIKE '%{%_set_%';

-- Convert loop.index to forloop.index (and related)
UPDATE email_templates
SET html_content = REPLACE(html_content, 'loop.index0', 'forloop.index0'),
    text_content = REPLACE(text_content, 'loop.index0', 'forloop.index0'),
    updated_at = NOW()
WHERE html_content LIKE '%loop.index0%'
   OR text_content LIKE '%loop.index0%';

UPDATE email_templates
SET html_content = REPLACE(html_content, 'loop.index', 'forloop.index'),
    text_content = REPLACE(text_content, 'loop.index', 'forloop.index'),
    updated_at = NOW()
WHERE html_content LIKE '%loop.index%'
   OR text_content LIKE '%loop.index%';

UPDATE email_templates
SET html_content = REPLACE(html_content, 'loop.first', 'forloop.first'),
    text_content = REPLACE(text_content, 'loop.first', 'forloop.first'),
    updated_at = NOW()
WHERE html_content LIKE '%loop.first%'
   OR text_content LIKE '%loop.first%';

UPDATE email_templates
SET html_content = REPLACE(html_content, 'loop.last', 'forloop.last'),
    text_content = REPLACE(text_content, 'loop.last', 'forloop.last'),
    updated_at = NOW()
WHERE html_content LIKE '%loop.last%'
   OR text_content LIKE '%loop.last%';

UPDATE email_templates
SET html_content = REPLACE(html_content, 'loop.length', 'forloop.length'),
    text_content = REPLACE(text_content, 'loop.length', 'forloop.length'),
    updated_at = NOW()
WHERE html_content LIKE '%loop.length%'
   OR text_content LIKE '%loop.length%';

-- ============================================================================
-- Verification: Check for remaining Jinja2 syntax that needs manual review
-- ============================================================================
SELECT name,
       CASE
           WHEN html_content LIKE '%{%_elif_%' THEN 'Has elif'
           WHEN html_content LIKE '%loop.%' THEN 'Has loop.'
           WHEN html_content LIKE '%{%_set_%' THEN 'Has set'
           ELSE 'OK'
       END as jinja2_check,
       LENGTH(html_content) as html_length
FROM email_templates
ORDER BY name;

-- Show templates that were updated
SELECT name, updated_at, template_type
FROM email_templates
WHERE updated_at > NOW() - INTERVAL '1 minute'
ORDER BY name;
