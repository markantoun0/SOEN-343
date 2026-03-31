-- Check for duplicate PlaceIds
SELECT "PlaceId", COUNT(*) as count
FROM "MobilityLocations"
GROUP BY "PlaceId"
HAVING COUNT(*) > 1
ORDER BY count DESC;

